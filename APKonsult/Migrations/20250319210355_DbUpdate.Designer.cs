﻿// <auto-generated />
using System;
using APKonsult.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace APKonsult.Migrations
{
    [DbContext(typeof(APKonsultContext))]
    [Migration("20250319210355_DbUpdate")]
    partial class DbUpdate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.3");

            modelBuilder.Entity("APKonsult.Models.AfkStatusEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<long>("AfkEpoch")
                        .HasColumnType("INTEGER")
                        .HasColumnName("afk_epoch");

                    b.Property<string>("AfkMessage")
                        .HasMaxLength(70)
                        .HasColumnType("TEXT")
                        .HasColumnName("afk_message");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("AfkStatusEntity");
                });

            modelBuilder.Entity("APKonsult.Models.BlacklistedDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("guild_id");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("ban_reason");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.ToTable("Blacklist");
                });

            modelBuilder.Entity("APKonsult.Models.EventAction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ActionName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("action_name");

                    b.Property<string>("EventName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("event_name");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("guild_id");

                    b.Property<string>("LuaScript")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("lua_script");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("EventAction");
                });

            modelBuilder.Entity("APKonsult.Models.GuildConfigDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("discordId");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("TEXT")
                        .HasColumnName("prefix");

                    b.Property<bool>("StarboardActive")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboard_enabled");

                    b.Property<ulong?>("StarboardChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboard_channel");

                    b.Property<ulong?>("StarboardEmojiId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboard_emoji_id");

                    b.Property<string>("StarboardEmojiName")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT")
                        .HasColumnName("starboard_emoji_name");

                    b.Property<int?>("StarboardThreshold")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboard_threshold");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("Configs");
                });

            modelBuilder.Entity("APKonsult.Models.GuildDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.HasKey("Id");

                    b.ToTable("guilds", (string)null);
                });

            modelBuilder.Entity("APKonsult.Models.IncidentDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreationTimeStamp")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("guild_id");

                    b.Property<ulong>("ModeratorId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("moderator_id");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("reason");

                    b.Property<ulong>("TargetId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("target_id");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("TargetId");

                    b.ToTable("Incidents");
                });

            modelBuilder.Entity("APKonsult.Models.MessageTag", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("TEXT")
                        .HasColumnName("tag_data");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("tag_name");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("MessageTag");
                });

            modelBuilder.Entity("APKonsult.Models.QuoteDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("TEXT")
                        .HasColumnName("content");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT")
                        .HasColumnName("timestamp");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("discordGuildId");

                    b.Property<ulong>("QuotedUserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("quotedUserId");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("UserId");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("APKonsult.Models.ReminderDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("channelId");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("creationTime");

                    b.Property<DateTime>("ExecutionTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("executionTime");

                    b.Property<bool>("IsPrivate")
                        .HasColumnType("INTEGER")
                        .HasColumnName("isPrivate");

                    b.Property<ulong>("MentionedChannel")
                        .HasColumnType("INTEGER")
                        .HasColumnName("mentionedChannel");

                    b.Property<ulong>("MentionedMessage")
                        .HasColumnType("INTEGER")
                        .HasColumnName("MentionedMessage");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("messageId");

                    b.Property<string>("ReminderText")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("reminderText");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("userId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Reminders");
                });

            modelBuilder.Entity("APKonsult.Models.StarboardMessageDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<ulong>("DiscordChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("discordChannelId");

                    b.Property<ulong>("DiscordGuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("discordGuildId");

                    b.Property<ulong>("DiscordMessageId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("discordMessageId");

                    b.Property<ulong>("StarboardChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboardChannelId");

                    b.Property<ulong>("StarboardGuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboardGuildId");

                    b.Property<ulong>("StarboardMessageId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starboardMessageId");

                    b.Property<int>("Stars")
                        .HasColumnType("INTEGER")
                        .HasColumnName("starCount");

                    b.HasKey("Id");

                    b.ToTable("Starboard");
                });

            modelBuilder.Entity("APKonsult.Models.TrackingDbEntity", b =>
                {
                    b.Property<ulong>("ConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<uint>("BeenFlagged")
                        .HasColumnType("INTEGER")
                        .HasColumnName("items_flagged");

                    b.Property<long>("CreationEpoch")
                        .HasColumnType("INTEGER")
                        .HasColumnName("creation_epoch");

                    b.Property<string>("EditorList")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("editor_list");

                    b.Property<ulong?>("GuildDbEntityId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("name");

                    b.Property<string>("RegexPattern")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("tracking_regex");

                    b.Property<ulong>("ReportChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("report_channel");

                    b.Property<ulong>("SourceChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("channel_id");

                    b.HasKey("ConfigId");

                    b.HasIndex("GuildDbEntityId");

                    b.HasIndex("GuildId");

                    b.HasIndex("Name");

                    b.ToTable("TrackingDbEntity");
                });

            modelBuilder.Entity("APKonsult.Models.UserDbEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<bool>("IsBotAdmin")
                        .HasColumnType("INTEGER")
                        .HasColumnName("is_bot_admin");

                    b.Property<string>("ReactionEmoji")
                        .HasColumnType("TEXT")
                        .HasColumnName("reaction_emoji");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("APKonsult.Models.VoiceAlert", b =>
                {
                    b.Property<ulong>("AlertId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("channel_id");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("guild_id");

                    b.Property<bool>("IsRepeatable")
                        .HasColumnType("INTEGER")
                        .HasColumnName("is_repeatable");

                    b.Property<DateTimeOffset?>("LastAlert")
                        .HasColumnType("TEXT")
                        .HasColumnName("last_alert");

                    b.Property<TimeSpan?>("MinTimeBetweenAlerts")
                        .HasColumnType("TEXT")
                        .HasColumnName("time_between");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("user_id");

                    b.HasKey("AlertId");

                    b.HasIndex("UserId");

                    b.ToTable("VoiceAlerts");
                });

            modelBuilder.Entity("APKonsult.Models.AfkStatusEntity", b =>
                {
                    b.HasOne("APKonsult.Models.UserDbEntity", "User")
                        .WithOne("AfkStatus")
                        .HasForeignKey("APKonsult.Models.AfkStatusEntity", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("APKonsult.Models.EventAction", b =>
                {
                    b.HasOne("APKonsult.Models.GuildDbEntity", "Guild")
                        .WithMany("DefinedActions")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("APKonsult.Models.GuildConfigDbEntity", b =>
                {
                    b.HasOne("APKonsult.Models.GuildDbEntity", "Guild")
                        .WithOne("Settings")
                        .HasForeignKey("APKonsult.Models.GuildConfigDbEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("APKonsult.Models.IncidentDbEntity", b =>
                {
                    b.HasOne("APKonsult.Models.GuildDbEntity", "Guild")
                        .WithMany("Incidents")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("APKonsult.Models.UserDbEntity", "TargetUser")
                        .WithMany("Incidents")
                        .HasForeignKey("TargetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("TargetUser");
                });

            modelBuilder.Entity("APKonsult.Models.MessageTag", b =>
                {
                    b.HasOne("APKonsult.Models.UserDbEntity", "User")
                        .WithMany("MessageAliases")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("APKonsult.Models.QuoteDbEntity", b =>
                {
                    b.HasOne("APKonsult.Models.GuildDbEntity", "Guild")
                        .WithMany("Quotes")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("APKonsult.Models.ReminderDbEntity", b =>
                {
                    b.HasOne("APKonsult.Models.UserDbEntity", "User")
                        .WithMany("Reminders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("APKonsult.Models.TrackingDbEntity", b =>
                {
                    b.HasOne("APKonsult.Models.GuildDbEntity", null)
                        .WithMany("TrackingConfigurations")
                        .HasForeignKey("GuildDbEntityId");
                });

            modelBuilder.Entity("APKonsult.Models.VoiceAlert", b =>
                {
                    b.HasOne("APKonsult.Models.UserDbEntity", "User")
                        .WithMany("VoiceAlerts")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("APKonsult.Models.GuildDbEntity", b =>
                {
                    b.Navigation("DefinedActions");

                    b.Navigation("Incidents");

                    b.Navigation("Quotes");

                    b.Navigation("Settings")
                        .IsRequired();

                    b.Navigation("TrackingConfigurations");
                });

            modelBuilder.Entity("APKonsult.Models.UserDbEntity", b =>
                {
                    b.Navigation("AfkStatus");

                    b.Navigation("Incidents");

                    b.Navigation("MessageAliases");

                    b.Navigation("Reminders");

                    b.Navigation("VoiceAlerts");
                });
#pragma warning restore 612, 618
        }
    }
}
